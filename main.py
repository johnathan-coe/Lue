import tkinter as tk

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, parent, string):
        super().__init__(parent)

        self.label = tk.Label(self, text=string)
        self.label.pack()


class App(tk.Tk):
    def __init__(self):
        super().__init__()

        self.items = [Item(self, "# Hello, World"), Item(self, "## Subheading")]
        [i.pack() for i in self.items]

        self.mainloop()

if __name__ == "__main__":
    App()