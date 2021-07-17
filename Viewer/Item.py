import tkinter as tk
import extensions
import themes
from PIL import ImageTk

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, parent, line):
        super().__init__(parent)
        self.s = parent.s
        self.cwd = parent.cwd

        self.label = tk.Label(self, wraplength=0)
        self.entryVal = tk.StringVar(self, line)
        self.entry = tk.Entry(self, textvariable=self.entryVal)

        self.bind('<Configure>', self.reflow)

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

    def assess(self):
        # Get info from string
        c, r = extensions.classify(self.renderedString)

        # Get styles
        styles = self.s.styles[c]
        packStyles = self.s.packStyles[c]
        
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
        themes.repurpose(self, self.s.appStyle['Frame'], 'bg')

        _, render, styles, _ = self.assess()

        # Extensions have no impact on the entry, so we can style it ourselves
        self.entry.configure(**styles)
        themes.repurpose(self.entry, styles, 'fg', 'insertbackground')

        # Hand off to rendering function
        rendered = render(self.renderedString, styles, self.cwd)

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
