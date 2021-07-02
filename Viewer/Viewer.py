from Viewer.Item import Item
from Viewer.VertFrame import VerticalScrolledFrame
import themes
import tkinter as tk

class Viewer(VerticalScrolledFrame):
    def __init__(self, parent):
        super().__init__(parent)
        self.items = []
        self.s = None
        
    def clear(self):
        [i.pack_forget() for i in self.items]
        self.items = []

    def append(self, line):
        i = Item(self)
        i.entryVal.set(line.rstrip())
        i.set()
        self.items.append(i)

        i.pack(fill=tk.X)
        return i

    def insert(self, priorItem):
        index = self.items.index(priorItem) + 1

        # Take everything after the item off the screen
        for item in self.items[index:]:
            item.pack_forget()

        # Add the iten to the array
        i = Item(self)
        i.set()
        self.items.insert(index, i)

        # Repack
        for item in self.items[index:]:
            item.pack(fill=tk.X)

        self.move(priorItem, +1)

    def loadFromFile(self, fileName):
        self.clear()

        with open(fileName, 'r') as f:
            for line in f.readlines():
                if not line.rstrip(): continue
                self.append(line)

    def saveToFile(self, fileName):
        with open(fileName, 'w') as f:
            f.writelines('\n'.join([l.entryVal.get() for l in self.items if l.entryVal.get()]))
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

    def remove(self, item):
        self.items.remove(item)
        item.pack_forget()

    def move(self, item, direction):
        # We can't move from an item that is not in editing mode
        if not item.editing:
            return
            
        to = self.items.index(item) + direction

        if to < 0:
            return
        elif to < len(self.items):
            self.update_idletasks()
            self.scroll_to(self.items[to])

            # Move to an existing item
            self.items[to].edit()

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
