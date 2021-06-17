import tkinter as tk
import config
import themes
import extensions
from exporters import HTML

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, app, parent):
        super().__init__(parent)
        
        self.app = app
        self.string = ''
    
        self.label = tk.Label(self, wraplength=500)
        self.entryVal = tk.StringVar()
        self.entry = tk.Entry(self, textvariable=self.entryVal)

        move = lambda d: lambda e: self.app.move(self, d)

        # Keyboard bindings
        self.entry.bind('<Return>', move(+1))
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # Style components and switch to editing mode
        self.edit()

    def style(self):
        themes.repurpose(self, self.app.s.appStyle['Frame'], 'bg')

        # Get info from string
        c, r = extensions.classify(self.string)

        self.entry.configure(**self.app.s.styles[c])
        themes.repurpose(self.entry, self.app.s.styles[c], 'fg', 'insertbackground')

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Hand off to rendering function
        r.render(self, self.app.s.styles[c])

    def set(self):
        # If we've updated the entry, update the label
        if self.string != self.entryVal.get():
            self.string = self.entryVal.get()
            self.style()
            
        c, _, = extensions.classify(self.string)
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
        self.itemFrame = tk.Frame(self)
        self.itemFrame.pack(fill=tk.BOTH, expand=True)        
        self.items = []
        self.style(config.THEME)

        self.loadFromFile('welcome.pnm')

        self.attachMenuBar()
        self.mainloop()

    def loadFromFile(self, fileName):
        with open(fileName, 'r') as f:
            for line in f.readlines():
                i = Item(self, self.itemFrame)
                i.entryVal.set(line.rstrip())
                i.set()
                self.items.append(i)

        end = Item(self, self.itemFrame)
        end.style()
        self.items.append(end)
        [i.pack(fill=tk.X) for i in self.items]

    def attachMenuBar(self):
        # Define a menu
        menu = tk.Menu(self)
        self.config(menu=menu)
        
        fileMenu = tk.Menu(menu)
        fileMenu.add_command(label="Open")
        fileMenu.add_command(label="Save")
        menu.add_cascade(label="File", menu=fileMenu)

        themeMenu = tk.Menu(menu)
        for theme in config.THEMES:
            themeMenu.add_command(label=theme.split('/')[-1], command=lambda t=theme: self.style(t))
        menu.add_cascade(label="Theme", menu=themeMenu)

        exportMenu = tk.Menu(menu)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(self))
        menu.add_cascade(label="Export", menu=exportMenu)

    def style(self, theme):
        self.s = themes.Theme(theme)

        self.configure(**self.s.appStyle['Window'])
        self.itemFrame.configure(**self.s.appStyle['Frame'])
        
        for i in self.items:
            i.style()

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
            newItem.style()
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            if not item.string:
                item.pack_forget()
                self.items.remove(item)
            
if __name__ == "__main__":
    App()