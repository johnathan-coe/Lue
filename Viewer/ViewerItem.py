"""
An item in the context of a viewer, adding key bindings for movement.
"""
from .Item import Item
from tkinter import INSERT

class ViewerItem(Item):
    def __init__(self, frame, line):
        super().__init__(frame, line)
        self.frame = frame

        move = lambda d: lambda e: self.frame.move(self, d)

        # Keyboard bindings
        self.entry.bind('<BackSpace>', self.back)
        self.entry.bind('<Return>', self.enter)
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # Move to this item on click
        self.label.bind('<Button-1>', self.moveTo)

    def moveTo(self, e):
        # Set all items
        [self.frame.setItem(i) for i in self.frame.items]
        
        # Edit this one
        self.edit()

        # Notify unsaved changes
        self.frame.unsaved = True

    def enter(self, e=None):
        # Pressing enter inserts an item after this one
        self.frame.insert(self)

    def back(self, e=None):
        # Pressing backpace at the start of a widget moves to the previous item
        if self.entry.index(INSERT) == 0:
            self.frame.move(self, -1)