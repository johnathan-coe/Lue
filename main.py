import tkinter as tk
import config
import themes
import extensions
from exporters import HTML
from VertFrame import VerticalScrolledFrame

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, frame):
        super().__init__(frame)
        self.frame = frame
        
        self.string = ''
    
        self.label = tk.Label(self, wraplength=500)
        self.entryVal = tk.StringVar()
        self.entry = tk.Entry(self, textvariable=self.entryVal)

        move = lambda d: lambda e: self.frame.move(self, d)

        # Keyboard bindings
        self.entry.bind('<Return>', move(+1))
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # Style components and switch to editing mode
        self.edit()

    def packStyles(self, c):
        self.label.pack_configure(**self.frame.s.packStyles.get(c, {}))

    def style(self):
        themes.repurpose(self, self.frame.s.appStyle['Frame'], 'bg')

        # Get info from string
        c, r = extensions.classify(self.string)

        self.entry.configure(**self.frame.s.styles[c])
        themes.repurpose(self.entry, self.frame.s.styles[c], 'fg', 'insertbackground')

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Apply positioning info
        self.packStyles(c)

        # Hand off to rendering function
        r.render(self, self.frame.s.styles[c])

    def set(self):
        # If we've updated the entry, update the label
        if self.string != self.entryVal.get():
            self.string = self.entryVal.get()
            self.style()
            
        c, _, = extensions.classify(self.string)
        # Remove entry box and place label on the screen 
        self.entry.pack_forget()

        self.label.pack()
        self.packStyles(c)

    def edit(self, e=None):
        self.label.pack_forget()
        self.entry.pack(fill=tk.X)
        
        c, _, = extensions.classify(self.string)
        self.entry.pack_configure(**self.frame.s.packStyles.get(c, {}))
        self.entry.focus_set()


class Viewer(VerticalScrolledFrame):
    def __init__(self, parent):
        super().__init__(parent)
        self.items = []
        
    def loadFromFile(self, fileName):
        with open(fileName, 'r') as f:
            for line in f.readlines():
                i = Item(self)
                i.entryVal.set(line.rstrip())
                i.set()
                self.items.append(i)

        end = Item(self)
        end.style()
        self.items.append(end)
        [i.pack(fill=tk.X) for i in self.items]

    def style(self, theme):
        self.s = themes.Theme(theme)

        self.configure(**self.s.appStyle['Frame'])
        themes.repurpose(self.canvas, self.s.appStyle['Frame'], 'bg')

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

            newItem = Item(self)
            newItem.style()
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            if not item.string:
                item.pack_forget()
                self.items.remove(item)

class App(tk.Tk):
    def __init__(self):
        super().__init__()

        self.itemFrame = Viewer(self)
        self.itemFrame.style(config.THEME)
        self.itemFrame.loadFromFile('welcome.pnm')
        self.itemFrame.pack(fill=tk.BOTH, expand=True)
        
        self.attachMenuBar()
        self.mainloop()

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
            themeMenu.add_command(label=theme.split('/')[-1], command=lambda t=theme: self.itemFrame.style(t))
        menu.add_cascade(label="Theme", menu=themeMenu)

        exportMenu = tk.Menu(menu)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(self.itemFrame))
        menu.add_cascade(label="Export", menu=exportMenu)
            
if __name__ == "__main__":
    App()