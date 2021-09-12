from Viewer.ViewerItem import ViewerItem
from Viewer.VertFrame import VerticalScrolledFrame
import themes
import tkinter as tk
import os

class Viewer(VerticalScrolledFrame):
    def __init__(self, parent):
        super().__init__(parent)
        self.items = []
        self.s = None
        self.cwd = os.getcwd()
        self.unsaved = False
        
    def clear(self):
        """
        Remove everything from the viewer
        """

        [i.pack_forget() for i in self.items]
        self.items = []

    def append(self, line):
        """
        Add an item to the top of the viewer and return it
        """

        i = ViewerItem(self, line.rstrip())
        self.items.append(i)
        i.pack(fill=tk.X)
        return i

    def insert(self, priorItem):
        """
        Insert a new, empty item into the viewer, following another one.
        """

        index = self.items.index(priorItem) + 1
        # Ignore if the last element is empty and we're trying to add to the end
        if index == len(self.items) and not priorItem.entryVal.get():
            return

        # Take everything after the item off the screen
        for item in self.items[index:]:
            item.pack_forget()

        # Add the item to the array
        i = ViewerItem(self, "")
        self.items.insert(index, i)

        # Repack
        for item in self.items[index:]:
            item.pack(fill=tk.X)

        self.move(priorItem, +1)

    def updateCwd(self, d):
        self.cwd = d
        for i in self.items:
            i.cwd = d
            i.updateLabel()

    def loadFromFile(self, fileName):
        """
        Load a file into the viewer, given an absolute filename
        """
        
        self.clear()
        self.updateCwd(os.path.dirname(fileName))
        
        with open(fileName, 'r') as f:
            for line in f.readlines():
                if not line.rstrip(): continue
                self.append(line)

    def saveToFile(self, fileName):
        """
        Save the content of the viewer to a file, given a filename
        """
        self.updateCwd(os.path.dirname(fileName))

        with open(fileName, 'w') as f:
            f.writelines('\n'.join([l.entryVal.get() for l in self.items if l.entryVal.get()]))
            f.write('\n')
        
        self.unsaved = False

    def style(self, theme):
        """
        Apply a theme to the viewer and its content
        """
        if self.s:
            themes.validator.external(self.s, theme)

        self.s = theme

        self.configure(**self.s.appStyle['Frame'])
        themes.repurpose(self.canvas, self.s.appStyle['Frame'], 'bg')
        themes.repurpose(self.inner, self.s.appStyle['Frame'], 'bg')

        for i in self.items:
            i.s = self.s
            i.updateLabel()
            i.packWidget()

    def remove(self, item):
        """
        Remove an item from the viewer.
        """
        
        self.items.remove(item)
        item.pack_forget()

    def setItem(self, item):
        """
        Sets an item, removing if necessary
        """
        
        if not item.entryVal.get():
            self.items.remove(item)
            item.pack_forget()

        item.set()


    def move(self, item, direction):
        """
        Attempt a move in a particular direction, given an element.
        """
        
        # We can't move from an item that is not in editing mode
        if not item.editing:
            return
            
        to = self.items.index(item) + direction

        if to < 0:
            return
        elif to < len(self.items):
            # Move to an existing item
            self.items[to].edit()
            self.setItem(item)
            self.update_idletasks()
            self.scroll_to(self.items[to])

        elif not item.entryVal.get():
            # Ignore if we are going beyond the end with an empty entry
            return
        else:
            self.insert(item)

        self.unsaved = True
