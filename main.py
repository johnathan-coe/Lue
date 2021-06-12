import tkinter as tk
from extensions.PlainText import PlainText
import config
import themes
import extensions
from exporters import HTML

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, app, parent, string=""):
        super().__init__(parent)
        self.app = app
        self.string = string

        # Repurpose frame bg for item bg
        themes.repurpose(self, app.s.appStyle['Frame'], 'bg')
       
        self.label = tk.Label(self)
        self.entry = tk.Entry(self)

        move = lambda d: lambda e: self.app.move(self, d)

        # Keyboard bindings
        self.entry.bind('<Return>', move(+1))
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # Style components and switch to editing mode
        self.style()
        self.edit()

    def style(self):
        # Get info from string
        c, s, r = extensions.classify(self.string)

        self.entry.configure(**self.app.s.styles[c])
        # Repurpose fg for insertbackground
        themes.repurpose(self.entry, self.app.s.styles[c], 'fg', 'insertbackground')

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Hand off to rendering function
        r.render(self, self.app.s.styles[c], s)

    def set(self):
        # If we've updated the entry, update the label
        if self.string != self.entry.get():
            self.string = self.entry.get()
            self.style()
            
        c, _, _ = extensions.classify(self.string)
        # Remove entry box and place label on the screen 
        self.entry.pack_forget()
        self.label.pack(**self.app.s.packStyles.get(c, {}))

    def edit(self, e=None):
        self.label.pack_forget()
        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.s = themes.Theme(config.THEME)

        self.configure(**self.s.appStyle['Window'])

        self.itemFrame = tk.Frame(self, **self.s.appStyle['Frame'])
        self.itemFrame.pack(fill=tk.BOTH, expand=True)

        self.items = [Item(self, self.itemFrame)]
        self.items[-1].pack(fill=tk.X)

        tk.Button(self, text='Convert to HTML', command=lambda: HTML.export(self)).pack()

        self.mainloop() 

    def move(self, item, direction):
        to = self.items.index(item) + direction
        if to < 0:
            pass
        elif to < len(self.items):
            item.set()
            self.items[to].edit()
        else:
            item.set()

            newItem = Item(self, self.itemFrame)
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            if not item.string:
                item.pack_forget()
                self.items.remove(item)
            
if __name__ == "__main__":
    App()