//! Builds wasm3 from the sources placed by run.py, with the options of its CMakeLists.txt for release builds.

use std::{env, fs, path::PathBuf};

fn main() {
    let source = PathBuf::from(env::var("WASM3_SOURCE").expect("WASM3_SOURCE is not set")).join("source");
    println!("cargo:rerun-if-env-changed=WASM3_SOURCE");
    println!("cargo:rerun-if-env-changed=WASM3_CLANG");

    let mut build = cc::Build::new();
    build
        .compiler(env::var("WASM3_CLANG").expect("WASM3_CLANG is not set"))
        .include(&source)
        .opt_level(3)
        .warnings(false)
        .flag("-fomit-frame-pointer")
        .flag("-fno-stack-check")
        .flag("-fno-stack-protector")
        .flag("-fno-asynchronous-unwind-tables");

    for entry in fs::read_dir(&source).unwrap() {
        let path = entry.unwrap().path();
        let name = path.file_name().unwrap().to_string_lossy().into_owned();
        if name.starts_with("m3_") && !name.starts_with("m3_api_") && name.ends_with(".c") {
            build.file(path);
        }
    }

    build.compile("wasm3");
}
