import tkinter as tk
from sympy import preview
from io import BytesIO
from PIL import ImageTk, Image

textStyles = {
    'h1': {
        'font': ('Segoe UI Semibold', 30),
        'bg': '#171717',
        'fg': '#e6e6e6'
    },
    'h2': {
        'font': ('Segoe UI Semibold', 25),
        'bg': '#171717',
        'fg': '#e6e6e6'
    },
    'body': {
        'font': ('Segoe UI', 12),
        'bg': '#171717',
        'fg': '#e6e6e6'
    }
}

packStyles = {
    'tex': {
        'padx': 5,
        'pady': 5
    }
}

def texMath(item, math):
    #try:
    out = BytesIO()
    
    preamble = "\n".join([
        "\\documentclass[17pt]{extarticle}",
        "\\usepackage{xcolor}",
        "\\pagenumbering{gobble}",
        "\\begin{document}",
        "\\definecolor{bg}{HTML}{171717}",
        "\\definecolor{fg}{HTML}{e6e6e6}",
        "\\color{fg}",
        "\\pagecolor{bg}"
    ])
    preview(math, output='png', viewer='BytesIO', preamble=preamble, outputbuffer=out, euler=False)

    img = Image.open(out)
    item.label.image = ImageTk.PhotoImage(img)
    item.label.configure(image=item.label.image, bg="#171717")
    #except RuntimeError:
        #item.label.config(text="TeX Error!")

extensions = {
    'tex': texMath
}

def classify(string):
    if string.startswith('##'):
        return "h2", string[2:].strip()
    elif string.startswith('#'):
        return "h1", string[1:].strip()
    elif string.startswith('$'):
        return "tex", '$$' + string[1:].strip() + '$$'

    return "body", string

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, parent):
        super().__init__(parent, bg="#171717")
        self.parent = parent
        self.string = ""

        self.label = tk.Label(self)

        self.entry = tk.Entry(self)
        self.entry.bind('<Return>', self.advance)
        self.entry.bind('<Down>', self.advance)
        self.entry.bind('<Up>', self.retreat)
        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()
        self.entry.configure(**textStyles['body'])

    def advance(self, e):
        self.set()
        self.parent.advance(self)

    def retreat(self, e):
        self.set()
        self.parent.retreat(self)

    def set(self):
        self.entry.pack_forget()

        # Compute label styling and content
        c, s = classify(self.entry.get())

        if self.string != self.entry.get():
            self.string = self.entry.get()
    
            if c in extensions:
                extensions[c](self, s)
            else:
                self.entry.configure(**textStyles[c])
                self.label.image = None
                self.label.configure(image='', text=s, **textStyles[c])

        self.label.pack(anchor=tk.W, **packStyles.get(c, {}))

    def edit(self, e=None):
        self.label.pack_forget()

        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.configure(bg="#171717")

        self.items = [Item(self)]
        self.items[-1].pack(fill=tk.X)

        self.mainloop()

    def advance(self, item):
        if item is self.items[-1]:
            self.items.append(Item(self))
            self.items[-1].pack(fill=tk.X)
        else:
            nextIndex = self.items.index(item) + 1
            self.items[nextIndex].edit()

        if not item.string:
            item.pack_forget()
            self.items.remove(item)

    def retreat(self, item):
        if item is not self.items[0]:
            prevIndex = self.items.index(item) - 1
            self.items[prevIndex].edit()
        else:
            item.edit()

        if not item.string:
            item.pack_forget()
            self.items.remove(item)

if __name__ == "__main__":
    App()