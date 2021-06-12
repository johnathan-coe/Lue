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

        # If there is a background set on the window,
        #   apply it to the label
        if 'bg' in self.app.s.appStyle['Frame']:
            self.config(bg=self.app.s.appStyle['Frame']['bg'])

        self.label = tk.Label(self)
        self.entry = tk.Entry(self)

        # Keyboard bindings
        self.entry.bind('<Return>', self.advance)
        self.entry.bind('<Down>', self.advance)
        self.entry.bind('<Up>', self.retreat)

        # Style components and switch to editing mode
        self.style()
        self.edit()
      
    def advance(self, e):
        self.set()
        self.app.move(self, +1)

    def retreat(self, e):
        self.set()
        self.app.move(self, -1)

    def style(self):
        # Get info from string
        c, s, r = extensions.classify(self.string)

        self.entry.configure(**self.app.s.styles[c])
        if 'fg' in self.app.s.styles[c]:
            self.entry.configure(insertbackground=self.app.s.styles[c]['fg'])

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

    def remove(self, item):
        item.pack_forget()
        self.items.remove(item)

    def move(self, item, direction):
        to = self.items.index(item) + direction
        if to < 0:
            item.edit()
        elif to < len(self.items):
            self.items[to].edit()
        else:
            newItem = Item(self, self.itemFrame)
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            if not item.string:
                self.remove(item)
            

if __name__ == "__main__":
    App()