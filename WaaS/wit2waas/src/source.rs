use crate::utils::*;
use anyhow::Result;
use wit_parser::{Docs, Resolve};

#[derive(Default)]
pub struct Source {
    content: String,
    indent: u32,
}

impl Source {
    pub fn push_line(&mut self, line: &str) {
        self.content.push_str(&"    ".repeat(self.indent as usize));
        self.content.push_str(line);
        self.content.push_str("\n");
    }

    pub fn push_line_empty(&mut self) {
        self.content.push_str("\n");
    }

    pub fn open_block(&mut self) {
        self.push_line("{");
        self.indent += 1;
    }

    pub fn close_block(&mut self) {
        self.indent -= 1;
        self.push_line("}");
    }

    pub fn finish(self) -> String {
        self.content
    }

    pub fn push_docs(&mut self, docs: &Docs) {
        if let Some(docs) = &docs.contents {
            self.push_line("/// <summary>");
            for line in docs.lines() {
                self.push_line(&format!("///     {}", line));
            }
            self.push_line("/// </summary>");
        }
    }

    pub fn push_alias(&mut self, target: &str, name: &str, resolve: &Resolve) -> Result<()> {

        self.push_line(&format!("[global::WaaS.ComponentModel.Binding.ComponentAlias(typeof({}))]", target));
        self.push_line(&format!("public readonly partial struct {}", to_upper_camel(name)));
        self.open_block();
        {
            self.push_line(&format!("private readonly {} value;", target));
            self.push_line(&format!("private {} ({} value) => this.value = value;", to_upper_camel(name), target));
            self.push_line(&format!("public static implicit operator {}({} value) => new (value);", to_upper_camel(name), target));
            self.push_line(&format!("public static implicit operator {}({} value) => value.value;", target, to_upper_camel(name)));
        }
        self.close_block();
        self.push_line_empty();
        
        Ok(())
    }
}