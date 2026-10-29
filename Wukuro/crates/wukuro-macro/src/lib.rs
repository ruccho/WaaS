use proc_macro2::{Literal, Span, TokenStream};
use quote::{format_ident, quote};
use syn::{FnArg, GenericArgument, Ident, ItemFn, Pat, PathArguments, Type, parse_macro_input};

/// WIT of `wukuro:core`, resolved when this crate is compiled so that scripts do not depend on the project layout.
const WUKURO_CORE_WIT: &str = concat!(env!("CARGO_MANIFEST_DIR"), "/wit/wukuro-core");

/// Prefix of the package of each entry point. An entry point `foo_bar` is exported as
/// `wukuro-entry:foo-bar/entry` with a function `run`.
const ENTRY_PACKAGE_PREFIX: &str = "wukuro-entry";

struct Parameter {
    name: Ident,
    wit_name: String,
    wit_type: String,
    ty: Type,
    /// `T` of `&ObjectOf<T>`
    object_type: Option<Type>,
}

/// Exports the function as an entry point of the script.
/// A script file can have multiple entry points, which share the items in the file.
///
/// Parameters can be `&Context`, primitive types, `String`, `&Action`, `&Object` and `&ObjectOf<T>`.
/// For `&ObjectOf<T>`, the type name of `T` is recorded in the custom section `wukuro-entry-types`
/// so that the inspector accepts only objects of the type.
/// Their values are given by the arguments set to the script reference in the inspector,
/// matched by the parameter name.
#[proc_macro_attribute]
pub fn entry(
    _attr: proc_macro::TokenStream,
    item: proc_macro::TokenStream,
) -> proc_macro::TokenStream {
    let func = parse_macro_input!(item as ItemFn);
    match expand_entry(&func) {
        Ok(stream) => stream.into(),
        Err(error) => {
            let error = error.to_compile_error();
            quote!(#func #error).into()
        }
    }
}

fn expand_entry(func: &ItemFn) -> syn::Result<TokenStream> {
    let mut params = Vec::<Parameter>::new();
    for arg in &func.sig.inputs {
        let FnArg::Typed(pat_type) = arg else {
            return Err(syn::Error::new_spanned(arg, "entry points cannot take self"));
        };
        let Pat::Ident(pat_ident) = &*pat_type.pat else {
            return Err(syn::Error::new_spanned(&pat_type.pat, "parameters must be identifiers"));
        };
        let ty = (*pat_type.ty).clone();
        let object_type = to_object_type(&ty);
        let wit_type = if object_type.is_some() {
            "borrow<object>"
        } else {
            to_wit_type(&ty)
                .ok_or_else(|| syn::Error::new_spanned(&ty, "this type cannot be a parameter of an entry point"))?
        };
        params.push(Parameter {
            name: pat_ident.ident.clone(),
            wit_name: to_kebab_case(&pat_ident.ident.to_string()),
            wit_type: wit_type.to_string(),
            ty,
            object_type,
        });
    }

    let fn_name = &func.sig.ident;
    let entry_name = to_kebab_case(&fn_name.to_string());
    let package = format!("{ENTRY_PACKAGE_PREFIX}:{entry_name}");

    let wit_params = params
        .iter()
        .map(|p| format!("{}: {}", p.wit_name, p.wit_type))
        .collect::<Vec<_>>()
        .join(", ");
    let wit = format!(
        r#"
        package {package};

        world entry-world {{
            export entry;
        }}

        interface entry {{
            use wukuro:core/types.{{context, action, object}};

            run: func({wit_params});
        }}
        "#
    );
    let wit = Literal::string(&wit);
    let world = Literal::string(&format!("{package}/entry-world"));
    let core_wit = Literal::string(WUKURO_CORE_WIT);

    let module = format_ident!("__wukuro_entry_{}", fn_name);
    let package_module = Ident::new(&entry_name.replace('-', "_"), Span::call_site());
    let param_pats = params.iter().map(|p| {
        let name = &p.name;
        let ty = &p.ty;
        match &p.object_type {
            Some(_) => quote!(#name: &::wukuro::types::Object),
            None => quote!(#name: #ty),
        }
    });
    let param_args = params.iter().map(|p| {
        let name = &p.name;
        match &p.object_type {
            Some(_) => quote!(::wukuro::ObjectOf::from_ref(#name)),
            None => quote!(#name),
        }
    });

    // records of the object types of the parameters, read by the inspector
    let records = params.iter().filter_map(|p| {
        let object_type = p.object_type.as_ref()?;
        let entry = Literal::string(&entry_name);
        let parameter = Literal::string(&p.wit_name);
        let name = format_ident!("__WUKURO_ENTRY_TYPE_{}", p.name.to_string().to_uppercase());
        Some(quote! {
            #[used]
            #[unsafe(link_section = "wukuro-entry-types")]
            static #name: [u8; ::wukuro::__private::record_len(
                #entry, #parameter, <#object_type as ::wukuro::ObjectType>::TYPE_NAME)] =
                ::wukuro::__private::record(#entry, #parameter, <#object_type as ::wukuro::ObjectType>::TYPE_NAME);
        })
    });

    Ok(quote! {
        #func

        #[doc(hidden)]
        #[allow(non_snake_case, unused_imports)]
        mod #module {
            // the parameter types are written in the scope of the script
            use super::*;

            ::wit_bindgen::generate!({
                inline: #wit,
                path: #core_wit,
                with: {
                    "wukuro:core/types": ::wukuro::wukuro::core::types,
                },
                world: #world,
            });

            struct Entry;

            impl exports::wukuro_entry::#package_module::entry::Guest for Entry {
                fn run(#(#param_pats),*) {
                    super::#fn_name(#(#param_args),*);
                }
            }

            export!(Entry);

            #(#records)*
        }
    })
}

fn to_wit_type(ty: &Type) -> Option<&'static str> {
    let (is_reference, path) = match ty {
        Type::Reference(r) => match &*r.elem {
            Type::Path(path) => (true, path),
            _ => return None,
        },
        Type::Path(path) => (false, path),
        _ => return None,
    };
    let name = path.path.segments.last()?.ident.to_string();
    Some(match (is_reference, name.as_str()) {
        (true, "Context") => "borrow<context>",
        (true, "Action") => "borrow<action>",
        (true, "Object") => "borrow<object>",
        (false, "bool") => "bool",
        (false, "i8") => "s8",
        (false, "i16") => "s16",
        (false, "i32") => "s32",
        (false, "i64") => "s64",
        (false, "u8") => "u8",
        (false, "u16") => "u16",
        (false, "u32") => "u32",
        (false, "u64") => "u64",
        (false, "f32") => "f32",
        (false, "f64") => "f64",
        (false, "char") => "char",
        (false, "String") => "string",
        _ => return None,
    })
}

/// Returns `T` if the type is `&ObjectOf<T>`.
fn to_object_type(ty: &Type) -> Option<Type> {
    let Type::Reference(reference) = ty else { return None };
    let Type::Path(path) = &*reference.elem else { return None };
    let segment = path.path.segments.last()?;
    if segment.ident != "ObjectOf" {
        return None;
    }
    let PathArguments::AngleBracketed(arguments) = &segment.arguments else { return None };
    match arguments.args.first()? {
        GenericArgument::Type(object_type) => Some(object_type.clone()),
        _ => None,
    }
}

fn to_kebab_case(name: &str) -> String {
    name.trim_matches('_').replace('_', "-")
}
