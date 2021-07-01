from Viewer.Item import Item
from Viewer.VertFrame import VerticalScrolledFrame
import themes
import tkinter as tk

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
        self.items.append(end)
        
        [i.pack(fill=tk.X) for i in self.items]
        
    def style(self, theme):
        self.s = themes.Theme(theme)

        self.configure(**self.s.appStyle['Frame'])
        themes.repurpose(self.canvas, self.s.appStyle['Frame'], 'bg')

        for i in self.items:
            i.style()
            i.packStyles()

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