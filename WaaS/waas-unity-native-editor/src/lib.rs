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
            .select_world(id, if world.is_empty() { None } else { Some(world) })?;

        wit_component::embed_component_metadata(
            &mut wasm,
            &resolve,
            world,
            match encoding {
                StringEncoding::UTF8 => wit_component::StringEncoding::UTF8,
                StringEncoding::UTF16 => wit_component::StringEncoding::UTF16,
                StringEncoding::CompactUTF16 => wit_component::StringEncoding::CompactUTF16,
            },
        )?;
    }

    let mut encoder = ComponentEncoder::default();
    encoder = encoder.module(&wasm)?;

    let result = encoder.encode()?;
    Ok(result)
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
    Ok(wit_component::WitPrinter::default().print(resolve, main, &nested)?)
}
