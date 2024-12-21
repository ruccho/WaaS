use wit_parser::{Handle, Interface, Package, Resolve, Tuple, Type, TypeDef, TypeDefKind, TypeOwner, World};

pub fn to_upper_camel(name: &str) -> String {
    let mut result = String::new();
    let mut upper = true;
    for c in name.chars() {
        if c == '-' {
            upper = true;
        } else if upper {
            result.push(c.to_ascii_uppercase());
            upper = false;
        } else {
            result.push(c);
        }
    }
    result
}

pub fn to_lower_camel(name: &str) -> String {
    let mut result = String::new();
    let mut upper = false;
    for c in name.chars() {
        if c == '-' {
            upper = true;
        } else if upper {
            result.push(c.to_ascii_uppercase());
            upper = false;
        } else {
            result.push(c);
        }
    }
    result
}

pub trait ToWaas {
    fn to_waas(&self, resolve: &Resolve) -> anyhow::Result<String>;
}

impl ToWaas for Package {
    fn to_waas(&self, _resolve: &Resolve) -> anyhow::Result<String> {
        Ok(format!("{}.{}", to_upper_camel(&self.name.namespace), to_upper_camel(&self.name.name)))
    }
}

impl ToWaas for Type {
    fn to_waas(&self, resolve: &Resolve) -> anyhow::Result<String> {
        Ok(match self {
            Type::Bool => "bool".into(),
            Type::U8 => "byte".into(),
            Type::U16 => "ushort".into(),
            Type::U32 => "uint".into(),
            Type::U64 => "ulong".into(),
            Type::S8 => "sbyte".into(),
            Type::S16 => "short".into(),
            Type::S32 => "int".into(),
            Type::S64 => "long".into(),
            Type::F32 => "float".into(),
            Type::F64 => "double".into(),
            Type::Char => "global::WaaS.ComponentModel.Binding.ComponentChar".into(),
            Type::String => "string".into(),
            Type::Id(id) => {
                let ty = resolve.types.get(*id).unwrap();
                ty.to_waas(resolve)?
            }
        })
    }
}

impl ToWaas for TypeDef {
    fn to_waas(&self, resolve: &Resolve) -> anyhow::Result<String> {
        fn to_waas_from_name(ty: &TypeDef, resolve: &Resolve, prefix: &str) -> anyhow::Result<String> {
            match &ty.owner {
                TypeOwner::World(id) => {
                    let world = resolve.worlds.get(*id).unwrap();
                    Ok(format!("{}.{}{}", world.to_waas(resolve)?, prefix, to_upper_camel(&ty.name.clone().unwrap())))
                }
                TypeOwner::Interface(id) => {
                    let interface = resolve.interfaces.get(*id).unwrap();
                    Ok(format!("{}.{}{}", interface.to_waas(resolve)?, prefix, to_upper_camel(&ty.name.clone().unwrap())))
                }
                TypeOwner::None => Err(anyhow::anyhow!("type owner not set"))?,
            }
        }

        match &self.kind {
            TypeDefKind::Record(_) => to_waas_from_name(self, resolve, ""),
            TypeDefKind::Resource => Ok(format!("{}ResourceImpl", to_waas_from_name(self, resolve, "I")?)),
            TypeDefKind::Handle(element) => {
                match element {
                    Handle::Own(element) => Ok(format!("global::WaaS.ComponentModel.Binding.Owned<{}>", resolve.types.get(*element).unwrap().to_waas(resolve)?)),
                    Handle::Borrow(element) => Ok(format!("global::WaaS.ComponentModel.Binding.Borrowed<{}>", resolve.types.get(*element).unwrap().to_waas(resolve)?)),
                }
            }
            TypeDefKind::Flags(_) => to_waas_from_name(self, resolve, ""),
            TypeDefKind::Tuple(element) => {
                if let Some(_name) = &self.name {
                    // named tuple
                    return to_waas_from_name(self, resolve, "");
                }

                Ok(to_raw_tuple(element, resolve))
            }
            TypeDefKind::Variant(_) => to_waas_from_name(self, resolve, ""),
            TypeDefKind::Enum(_) => to_waas_from_name(self, resolve, ""),
            TypeDefKind::Option(element) => Ok(if element.is_value_type(resolve)? {
                format!("{}?", element.to_waas(resolve)?)
            } else {
                format!("global::WaaS.ComponentModel.Binding.Option<{}>", element.to_waas(resolve)?)
            }),
            TypeDefKind::Result(element) => {
                Ok(format!("global::WaaS.ComponentModel.Binding.Result<{}, {}>", match element.ok {
                    None => "global::WaaS.ComponentModel.Binding.None".to_string(),
                    Some(ty) => ty.to_waas(resolve)?
                }, match element.err {
                    None => "global::WaaS.ComponentModel.Binding.None".to_string(),
                    Some(ty) => ty.to_waas(resolve)?
                }))
            }
            TypeDefKind::List(element) => Ok(format!("global::System.ReadOnlyMemory<{}>", element.to_waas(resolve)?)),
            TypeDefKind::Future(_) => todo!(),
            TypeDefKind::Stream(_) => todo!(),
            TypeDefKind::Type(element) => {
                // println!("{:?}", &self.name);
                if element.is_alias_compatible(self, resolve)? {
                    to_waas_from_name(self, resolve, "")
                } else {
                    element.to_waas(resolve)
                }
            }
            TypeDefKind::Unknown => Err(anyhow::anyhow!("unknown type def kind"))?,
        }
    }
}

impl ToWaas for World {
    fn to_waas(&self, resolve: &Resolve) -> anyhow::Result<String> {
        let package = resolve.packages.get(self.package.unwrap()).unwrap();
        Ok(format!("{}.{}", package.to_waas(resolve)?, to_upper_camel(&self.name)))
    }
}

impl ToWaas for Interface {
    fn to_waas(&self, resolve: &Resolve) -> anyhow::Result<String> {
        // TODO: support inline interfaces
        let name = to_upper_camel(&self.name.clone().unwrap());
        if let Some(package_id) = self.package {
            let package = resolve.packages.get(package_id).unwrap();
            Ok(format!("{}.I{}", package.to_waas(resolve)?, name))
        } else {
            Ok(format!("global::I{}", name))
        }
    }
}

pub trait IsValueType {
    fn is_value_type(&self, resolve: &Resolve) -> anyhow::Result<bool>;
}

impl IsValueType for Type {
    fn is_value_type(&self, resolve: &Resolve) -> anyhow::Result<bool> {
        Ok(match self {
            Type::String => false,
            Type::Id(id) => {
                let ty = resolve.types.get(*id).unwrap();
                match &ty.kind {
                    TypeDefKind::Resource => false,
                    TypeDefKind::Option(element) => !element.is_value_type(resolve)?,
                    TypeDefKind::List(_) => false,
                    TypeDefKind::Future(_) => todo!(),
                    TypeDefKind::Stream(_) => todo!(),
                    TypeDefKind::Type(element) => {
                        if element.is_alias_compatible(ty, resolve)? {
                            true
                        } else {
                            element.is_value_type(resolve)?
                        }
                    }
                    TypeDefKind::Unknown => Err(anyhow::anyhow!("unknown type def kind"))?,
                    _ => true,
                }
            }
            _ => true
        })
    }
}

pub trait TypeExt {
    fn is_alias_compatible(&self, from: &TypeDef, resolve: &Resolve) -> anyhow::Result<bool>;
}

impl TypeExt for Type {
    fn is_alias_compatible(&self, from: &TypeDef, resolve: &Resolve) -> anyhow::Result<bool> {
        Ok(match self {
            Type::Id(element) => {
                let ty = resolve.types.get(*element).unwrap();


                if from.name == ty.name {
                    return Ok(false);
                }
                match &ty.kind {
                    TypeDefKind::Resource => false,
                    TypeDefKind::Type(element) => element.is_alias_compatible(ty, resolve)?,
                    TypeDefKind::Unknown => Err(anyhow::anyhow!("unknown type def kind"))?,
                    _ => true
                }
            }
            _ => true
        })
    }
}

pub fn to_raw_tuple(tuple: &Tuple, resolve: &Resolve) -> String {
    let mut result = "(".to_string();
    let mut first = true;
    for ty in &tuple.types
    {
        if first {
            first = false;
        } else {
            result.push_str(", ");
        }
        result.push_str(&ty.to_waas(resolve).unwrap());
    }
    result.push(')');
    result
}