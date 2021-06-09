import tkinter as tk

styles = {
    'h1': {
        'font': ('Segoe UI', 18)
    },
    'h2': {
        'font': ('Segoe UI', 14)
    },
    'body': {
        'font': ('Segoe UI', 11)
    }
}

def classify(string):
    if string.startswith('##'):
        return "h2", string[2:].strip()
    elif string.startswith('#'):
        return "h1", string[1:].strip()

    return "body", string

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, parent):
        super().__init__(parent)
        self.parent = parent
        self.string = ""

        self.label = tk.Label(self)
        self.label.bind('<Button-1>', self.edit)

        self.entry = tk.Entry(self)
        self.entry.bind('<Return>', self.advance)
        self.entry.bind('<Down>', self.advance)
        self.entry.bind('<Up>', self.retreat)
        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()

    def advance(self, e):
        self.set()
        self.parent.advance(self)

    def retreat(self, e):
        self.set()
        self.parent.retreat(self)

    def set(self):
        self.entry.pack_forget()

        self.string = self.entry.get()

        # Compute label styling and content
        c, s = classify(self.string)
        self.label.configure(text=s, **styles[c])
        self.label.pack(anchor=tk.W)

    def edit(self, e=None):
        self.parent.setActive(self)

        self.label.pack_forget()

        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()


class App(tk.Tk):
    def __init__(self):
        super().__init__()

        self.items = [Item(self)]
        self.items[-1].pack(fill=tk.X)

        self.mainloop()

    def setActive(self, item):
        for i in self.items:
            if i is not item:
                i.set()

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