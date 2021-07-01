from Viewer.Item import Item
from Viewer.VertFrame import VerticalScrolledFrame
import themes
import tkinter as tk

class Viewer(VerticalScrolledFrame):
    def __init__(self, parent):
        super().__init__(parent)
        self.items = []
        self.s = None
        
    def loadFromFile(self, fileName):
        # Remove the old items
        [i.pack_forget() for i in self.items]
        self.items = []

        with open(fileName, 'r') as f:
            for line in f.readlines():
                if not line.rstrip(): continue

                i = Item(self)
                i.entryVal.set(line.rstrip())
                i.set()
                self.items.append(i)
        
        [i.pack(fill=tk.X) for i in self.items]

        # Edit the first item
        self.items[0].edit()

    def saveToFile(self, fileName):
        with open(fileName, 'w') as f:
            f.writelines('\n'.join([l.entryVal.get() for l in self.items]))
            f.write('\n')
        
    def style(self, theme):
        self.s = theme

        self.configure(**self.s.appStyle['Frame'])
        themes.repurpose(self.canvas, self.s.appStyle['Frame'], 'bg')

        for i in self.items:
            i.style()
        
    def restyle(self, new):
        themes.validator.external(self.s, new)
        self.style(new)

    def move(self, item, direction):
        # We can't move from an item that is not in editing mode
        if not item.editing:
            return
            
        to = self.items.index(item) + direction

        if to < 0:
            return
        elif to < len(self.items):
            # Move to an existing item
            self.items[to].edit()

            self.update_idletasks()
            self.scroll_to(self.items[to])

            if not item.entryVal.get():
                self.items.remove(item)
                item.pack_forget()
        elif not item.entryVal.get():
            # Ignore if we are going beyond the end with an empty entry
            return
        else:
            # Add a new item to the end
            newItem = Item(self)
            newItem.style()
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            self.update_idletasks()
            self.scroll_to(newItem)

        # If we have moved, set the current item
        item.set()