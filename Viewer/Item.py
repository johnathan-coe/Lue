import tkinter as tk
import extensions
import themes

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
        self.editing = True

    def packStyles(self):
        c, _ = extensions.classify(self.string)
        styles = self.frame.s.packStyles.get(c, {})
        
        if self.editing:
            self.entry.pack_configure(**styles)
        else:
            self.label.pack_configure(**styles)

    def style(self):
        """
        Apply the relevant styling attributes to the label and 
        """

        themes.repurpose(self, self.frame.s.appStyle['Frame'], 'bg')

        # Get info from string
        c, r = extensions.classify(self.string)

        self.entry.configure(**self.frame.s.styles[c])
        themes.repurpose(self.entry, self.frame.s.styles[c], 'fg', 'insertbackground')

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Hand off to rendering function
        r.render(self, self.frame.s.styles[c])

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
