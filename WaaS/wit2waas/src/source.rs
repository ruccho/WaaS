#[derive(Default)]
pub struct Source {
    content: String,
    indent: u32
}

impl Source {
    pub fn push_line(&mut self, line: &str) {
        self.content.push_str(&"    ".repeat(self.indent as usize));
        self.content.push_str(line);
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
}