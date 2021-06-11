import tkinter as tk
from themes import loader
from extensions import texMath

styles, packStyles, appStyle = loader.load('themes/ms')

extensions = [texMath]

def renderText(item, style, string):
    item.label.configure(text=string, **style)

def classify(string):
    if string.startswith('##'):
        return "h2", string[2:].strip(), renderText
    elif string.startswith('#'):
        return "h1", string[1:].strip(), renderText

    for ext in extensions:
        if string.startswith(ext.PREFIX):
            return ext.lex(string)        

    return "body", string, renderText

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, parent):
        super().__init__(parent)

        # If there is a background set on the window,
        #   apply it to the label
        if 'bg' in appStyle['Window']:
            self.config(bg=appStyle['Window']['bg'])

        self.parent = parent
        self.string = ""

        self.label = tk.Label(self)

        self.entry = tk.Entry(self)
        self.entry.bind('<Return>', self.advance)
        self.entry.bind('<Down>', self.advance)
        self.entry.bind('<Up>', self.retreat)
        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()
        self.entry.configure(**styles['body'])
        if 'fg' in styles['body']:
            self.entry.configure(insertbackground=styles['body']['fg'])

    def advance(self, e):
        self.set()
        self.parent.advance(self)

    def retreat(self, e):
        self.set()
        self.parent.retreat(self)

    def set(self):
        self.entry.pack_forget()

        # Compute label styling and content
        c, s, r = classify(self.entry.get())

        # If we've updated the entry, update the label
        if self.string != self.entry.get():
            self.string = self.entry.get()

            self.entry.configure(**styles[c])
            if 'fg' in styles[c]:
                self.entry.configure(insertbackground=styles[c]['fg'])

            # Wipe any existing image
            self.label.image = None
            self.label.configure(image='')

            # Hand off to rendering function
            r(self, styles[c], s)

        self.label.pack(**packStyles.get(c, {}))

    def edit(self, e=None):
        self.label.pack_forget()

        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.configure(**appStyle['Window'])

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