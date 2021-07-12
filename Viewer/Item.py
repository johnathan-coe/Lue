import tkinter as tk
import extensions
import themes
from PIL import ImageTk

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, frame, line=""):
        super().__init__(frame)
        self.frame = frame

        self.label = tk.Label(self, wraplength=0)
        self.entryVal = tk.StringVar(self, line)
        self.entry = tk.Entry(self, textvariable=self.entryVal)

        self.bind('<Configure>', self.reflow)

        move = lambda d: lambda e: self.frame.move(self, d)

        # Keyboard bindings
        self.entry.bind('<BackSpace>', self.back)
        self.entry.bind('<Return>', self.enter)
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # The string that the label is displaying
        self.renderedString = None

        # Set this Item
        self.set()
        self.editing = False

    def get(self):
        return self.entryVal.get()

    def reflow(self, e=None):
        # Reflow the label when this widget is reconfigured
        self.label.configure(wraplength=e.width)

    def enter(self, e=None):
        # Pressing enter inserts an item after this one
        self.frame.insert(self)

    def back(self, e=None):
        # Pressing backpace at the start of a widget moves to the previous item
        if self.entry.index(tk.INSERT) == 0:
            self.frame.move(self, -1)

    def assess(self):
        # Get info from string
        c, r = extensions.classify(self.renderedString)

        # Get styles
        styles = self.frame.s.styles[c]
        packStyles = self.frame.s.packStyles[c]
        
        return c, r, styles, packStyles

    def packWidget(self):
        # Pack the widget with appropriate styles
        _, _, _, packStyles = self.assess()

        if self.editing:
            self.entry.pack_configure(fill=tk.X, **packStyles)
        else:
            self.label.pack_configure(**packStyles)

    def updateLabel(self):
        """
        Update label styling and content, given the content of the entry.
        """
        themes.repurpose(self, self.frame.s.appStyle['Frame'], 'bg')

        _, render, styles, _ = self.assess()

        # Extensions have no impact on the entry, so we can style it ourselves
        self.entry.configure(**styles)
        themes.repurpose(self.entry, styles, 'fg', 'insertbackground')

        # Hand off to rendering function
        rendered = render(self.renderedString, styles)

        # Apply rendered content to label
        text = ''
        self.label.image = ''
        
        if type(rendered) == str:
            text = rendered
        else:
            self.label.image = ImageTk.PhotoImage(rendered)
        
        self.label.configure(image=self.label.image, text=text, **styles)

    def set(self):
        self.editing = False
        self.entry.pack_forget()

        # If we've updated the entry, update the label
        if self.renderedString != self.entryVal.get():
            self.renderedString = self.entryVal.get()
            # Restyle
            self.updateLabel()

        self.packWidget()
        
    def edit(self, e=None):
        self.editing = True
        self.label.pack_forget()
        self.packWidget()
        self.entry.focus_set()
