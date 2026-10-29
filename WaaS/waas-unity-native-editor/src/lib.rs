use std::error::Error;
use std::ffi::{c_char, CStr};
use wit_component::ComponentEncoder;
use wit_parser::Resolve;

#[no_mangle]
pub unsafe extern "C" fn componentize(
    source: *const u8,
    source_len: usize,
    wit_path: *const c_char,
    world: *const c_char,
    encoding: StringEncoding,
    user_data: usize,
    on_success: extern "C" fn(usize, *const u8, usize),
    on_error: extern "C" fn(usize, *const c_char),
) {
    let result = componentize_inner(source, source_len, wit_path, world, encoding);
    match result {
        Ok(bytes) => on_success(user_data, bytes.as_ptr(), bytes.len()),
        Err(e) => {
            let error = std::ffi::CString::new(e.to_string()).unwrap();
            on_error(user_data, error.as_ptr());
        }
    }
}

unsafe fn componentize_inner(
    source: *const u8,
    source_len: usize,
    wit_path: *const c_char,
    world: *const c_char,
    encoding: StringEncoding,
) -> Result<Vec<u8>, Box<dyn Error>> {
    let wit_path = CStr::from_ptr(wit_path)
        .to_str()?;
    let world = CStr::from_ptr(world).to_str()?;

    let mut wasm = std::slice::from_raw_parts(source, source_len).to_vec();

    // An empty WIT path means the module already carries its component type metadata
    // (e.g. embedded by wit-bindgen), so nothing is embedded here.
    if !wit_path.is_empty() {
        let mut resolve = Resolve::default();
        let (id, _) = resolve.push_dir(wit_path)?;
        let world = resolve
            .select_world(&[id], if world.is_empty() { None } else { Some(world) })?;

        wit_component::embed_component_metadata(
            &mut wasm,
            &resolve,
            world,
            match encoding {
                StringEncoding::UTF8 => wit_component::StringEncoding::UTF8,
                StringEncoding::UTF16 => wit_component::StringEncoding::UTF16,
                StringEncoding::CompactUTF16 => wit_component::StringEncoding::CompactUTF16,
            },
            false,
        )?;
    }

    let mut encoder = ComponentEncoder::default();
    encoder.module(&wasm)?;

    // MoonBit's `println` imports `spectest.print_char`, which no component can satisfy.
    // It is bound to a no-op, as `println` of Rust is on wasm32-unknown-unknown.
    if imports_module(&wasm, "spectest")? {
        encoder.adapter("spectest", &spectest_adapter()?)?;
    }

    let result = encoder.encode()?;
    Ok(result)
}

fn imports_module(wasm: &[u8], module: &str) -> Result<bool, Box<dyn Error>> {
    for payload in wasmparser::Parser::new(0).parse_all(wasm) {
        if let wasmparser::Payload::ImportSection(reader) = payload? {
            for import in reader.into_imports() {
                if import?.module == module {
                    return Ok(true);
                }
            }
        }
    }
    Ok(false)
}

fn spectest_adapter() -> Result<Vec<u8>, Box<dyn Error>> {
    let mut adapter = wat::parse_str(r#"(module (func (export "print_char") (param i32)))"#)?;
    let mut resolve = Resolve::default();
    let package = resolve.push_str("spectest-adapter.wit", "package waas:spectest-adapter;
world adapter {}
")?;
    let world = resolve.select_world(&[package], None)?;
    wit_component::embed_component_metadata(
        &mut adapter,
        &resolve,
        world,
        wit_component::StringEncoding::UTF8,
        false,
    )?;
    Ok(adapter)
}

#[repr(u32)]
pub enum StringEncoding {
    UTF8,
    UTF16,
    CompactUTF16,
}

/// Decodes the type of a component and prints it as WIT.
#[no_mangle]
pub unsafe extern "C" fn decode_wit(
    source: *const u8,
    source_len: usize,
    user_data: usize,
    on_success: extern "C" fn(usize, *const u8, usize),
    on_error: extern "C" fn(usize, *const c_char),
) {
    let result = decode_wit_inner(std::slice::from_raw_parts(source, source_len));
    match result {
        Ok(text) => on_success(user_data, text.as_ptr(), text.len()),
        Err(e) => {
            let error = std::ffi::CString::new(e.to_string()).unwrap_or_default();
            on_error(user_data, error.as_ptr());
        }
    }
}

fn decode_wit_inner(bytes: &[u8]) -> Result<String, Box<dyn Error>> {
    let decoded = wit_component::decode(bytes)?;
    let resolve = decoded.resolve();
    let main = decoded.package();
    let nested = resolve
        .packages
        .iter()
        .map(|(id, _)| id)
        .filter(|id| *id != main)
        .collect::<Vec<_>>();
    let mut printer = wit_component::WitPrinter::default();
    printer.print(resolve, main, &nested)?;
    Ok(printer.output.to_string())
}

/// Generates MoonBit bindings of a world. Each generated file is passed to `on_file` with its path relative to the
/// output directory.
#[no_mangle]
pub unsafe extern "C" fn generate_moonbit_bindings(
    wit_path: *const c_char,
    world: *const c_char,
    project_name: *const c_char,
    user_data: usize,
    on_file: extern "C" fn(usize, *const c_char, *const u8, usize),
    on_error: extern "C" fn(usize, *const c_char),
) {
    let result = generate_moonbit_bindings_inner(wit_path, world, project_name);
    match result {
        Ok(files) => {
            for (name, contents) in files.iter() {
                let name = std::ffi::CString::new(name).unwrap_or_default();
                on_file(user_data, name.as_ptr(), contents.as_ptr(), contents.len());
            }
        }
        Err(e) => {
            let error = std::ffi::CString::new(e.to_string()).unwrap_or_default();
            on_error(user_data, error.as_ptr());
        }
    }
}

unsafe fn generate_moonbit_bindings_inner(
    wit_path: *const c_char,
    world: *const c_char,
    project_name: *const c_char,
) -> Result<wit_bindgen_core::Files, Box<dyn Error>> {
    let wit_path = CStr::from_ptr(wit_path).to_str()?;
    let world = CStr::from_ptr(world).to_str()?;
    let project_name = CStr::from_ptr(project_name).to_str()?;

    let mut resolve = Resolve::default();
    let (id, _) = resolve.push_dir(wit_path)?;
    let world = resolve.select_world(&[id], if world.is_empty() { None } else { Some(world) })?;

    let opts = wit_bindgen_moonbit::Opts {
        ignore_stub: true,
        ignore_module_file: true,
        project_name: Some(project_name.to_string()),
        ..Default::default()
    };
    let mut files = wit_bindgen_core::Files::default();
    opts.build().generate(&mut resolve, world, &mut files)?;
    Ok(files)
}
