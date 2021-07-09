import tkinter as tk
import extensions
import themes

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
        self.renderedString = ''

        # Style components and switch to editing mode
        self.set()
        self.editing = False

    def get(self):
        """
        Get the value of this Item as a string
        """
        return self.entryVal.get()

    def reflow(self, e=None):
        """
        Reflow the label when this widget is reconfigured
        """
        self.label.configure(wraplength=e.width)

    def enter(self, e=None):
        """
        Pressing enter inserts an item after this one
        """
        self.frame.insert(self)

    def back(self, e=None):
        """
        Pressing backpace at the start of a widget, moves to the previous item
        """

        # If we're deleting at the left of the entry
        if self.entry.index(tk.INSERT) == 0:
            self.frame.move(self, -1)

    def assess(self):
        # Get info from string
        c, r = extensions.classify(self.renderedString)

        # Get styles
        styles = self.frame.s.styles.get(c,
                    self.frame.s.styles.get("body",
                        {}
                    )
                )

        packStyles = self.frame.s.packStyles.get(c,
                    self.frame.s.packStyles.get("body",
                        {}
                    )
                )
        
        return c, r, styles, packStyles

    def packStyles(self):
        """
        Apply pack styles and pack the widget
        """

        _, _, _, packStyles = self.assess()
        
        if self.editing:
            self.entry.pack_configure(**packStyles)
        else:
            self.label.pack_configure(**packStyles)

    def style(self):
        """
        Apply the relevant styling attributes to the label 
        """

        themes.repurpose(self, self.frame.s.appStyle['Frame'], 'bg')

        _, r, styles, _ = self.assess()

        self.entry.configure(**styles)
        themes.repurpose(self.entry, styles, 'fg', 'insertbackground')

        # Wipe any existing content
        self.label.image = None
        self.label.configure(image='', text='')

        # Hand off to rendering function
        r.render(self.renderedString, self.label, styles)

        self.packStyles()

    def set(self):
        """
        Called to move this Item out of editing mode
        """

        self.editing = False

        # If we've updated the entry, update the label
        if self.renderedString != self.entryVal.get():
            self.renderedString = self.entryVal.get()
            # Restyle in case the element has changed its type
            self.style()

        # Remove entry box and place label on the screen 
        self.entry.pack_forget()
        self.packStyles()
        
    def edit(self, e=None):
        """
        Called to move this Item into editing mode.
        """

        self.editing = True
        self.label.pack_forget()

        self.style()
        self.entry.pack_configure(fill=tk.X)
        self.entry.focus_set()
