use std::path::Path;
use std::process::Command;

fn read(dir: &Path, file: &str) -> String {
    std::fs::read_to_string(dir.join("a/b").join(file)).unwrap()
}

#[test]
fn inline_interfaces_are_generated() {
    let wit_dir = Path::new(env!("CARGO_MANIFEST_DIR")).join("tests/inline-interface/wit");
    let out_dir = Path::new(env!("CARGO_TARGET_TMPDIR")).join("inline-interface-out");

    let status = Command::new(env!("CARGO_BIN_EXE_wit2waas"))
        .arg("--wit-dir")
        .arg(&wit_dir)
        .arg("--out")
        .arg(&out_dir)
        .status()
        .unwrap();
    assert!(status.success());

    // the name in the component of an inline interface is the plain name of the import / export
    let foo = read(&out_dir, "WFoo.g.cs");
    assert!(foo.contains("ComponentInterface(@\"foo\")"));
    assert!(foo.contains("public partial interface IWFoo"));

    let bar = read(&out_dir, "WBar.g.cs");
    assert!(bar.contains("ComponentInterface(@\"bar\")"));
    assert!(bar.contains("public partial interface IWBar"));
    assert!(bar.contains("ValueTask<A.B.IWBar.Local> G(A.B.ITypes.Point @p);"));

    // exported interfaces are exposed as properties of the world
    let world = read(&out_dir, "W.g.cs");
    assert!(world.contains("A.B.IWBar Bar { get; }"));
    assert!(world.contains("A.B.ITypes Types { get; }"));
}
