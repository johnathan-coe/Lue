import tkinter as tk
import extensions
import themes

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, frame, line=""):
        super().__init__(frame)
        self.frame = frame

        self.string = ''
    
        self.label = tk.Label(self, wraplength=0)
        self.entryVal = tk.StringVar()
        self.entryVal.set(line)
        self.entry = tk.Entry(self, textvariable=self.entryVal)

        self.bind('<Configure>', self.reflow)

        move = lambda d: lambda e: self.frame.move(self, d)

        # Keyboard bindings
        self.entry.bind('<BackSpace>', self.back)
        self.entry.bind('<Return>', self.enter)
        self.entry.bind('<Down>', move(+1))
        self.entry.bind('<Up>', move(-1))

        # Style components and switch to editing mode
        self.set()
        self.editing = False

    def reflow(self, e=None):
        self.label.configure(wraplength=e.width)

    def enter(self, e=None):
        self.frame.insert(self)

    def back(self, e=None):
        # If we're deleting at the left of the entry
        if self.entry.index(tk.INSERT) == 0:
            self.frame.move(self, -1)

    def packStyles(self):
        """
        Apply pack styles, in turn packing the widget
        """

        c, _ = extensions.classify(self.string)
        # Fallback on body styles then blank dict
        styles = self.frame.s.packStyles.get(c,
                    self.frame.s.packStyles.get("body",
                        {}
                    )
                )
        
        if self.editing:
            self.entry.pack_configure(**styles)
        else:
            self.label.pack_configure(**styles)

    def style(self):
        """
        Apply the relevant styling attributes to the label 
        """

        themes.repurpose(self, self.frame.s.appStyle['Frame'], 'bg')

        # Get info from string
        c, r = extensions.classify(self.string)

        # Fallback on body styles then blank dict
        styles = self.frame.s.styles.get(c,
                    self.frame.s.styles.get("body",
                        {}
                    )
                )

        self.entry.configure(**styles)
        themes.repurpose(self.entry, styles, 'fg', 'insertbackground')

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Hand off to rendering function
        r.render(self, styles)

        self.packStyles()

    def set(self):
        """
        Called to move this Item out of editing mode
        """
        self.editing = False

        # If we've updated the entry, update the label
        if self.string != self.entryVal.get():
            self.string = self.entryVal.get()
            # Restyle in case the element has changed its type
            self.style()

        # Remove entry box and place label on the screen 
        self.entry.pack_forget()

        self.label.pack()
        self.packStyles()
        
    def edit(self, e=None):
        """
        Called to move this Item into editing mode.
        """
        self.editing = True

        self.style()
        self.label.pack_forget()
        self.entry.pack(fill=tk.X)
        self.entry.focus_set()
