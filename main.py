import tkinter as tk
from themes import loader
from extensions.PlainText import PlainText
from extensions import TexMath

styles, packStyles, appStyle = loader.load('themes/ms')

extensions = [TexMath]

def classify(string):
    if string.startswith('##'):
        return "h2", string[2:].strip(), PlainText
    elif string.startswith('#'):
        return "h1", string[1:].strip(), PlainText

    for ext in extensions:
        if string.startswith(ext.PREFIX):
            return ext.lex(string)        

    return "body", string, PlainText

# Each item is either a frame or entry depending on state
class Item(tk.Frame):
    def __init__(self, app, parent, string=""):
        super().__init__(parent)

        # If there is a background set on the window,
        #   apply it to the label
        if 'bg' in appStyle['Frame']:
            self.config(bg=appStyle['Frame']['bg'])

        self.app = app
        self.string = string

        self.label = tk.Label(self)
        self.entry = tk.Entry(self)

        # Keyboard bindings
        self.entry.bind('<Return>', self.advance)
        self.entry.bind('<Down>', self.advance)
        self.entry.bind('<Up>', self.retreat)

        # Style components and switch to editing mode
        self.style()
        self.edit()
      
    def advance(self, e):
        self.set()
        self.app.move(self, +1)

    def retreat(self, e):
        self.set()
        self.app.move(self, -1)

    def style(self):
        # Get info from string
        c, s, r = classify(self.string)

        self.entry.configure(**styles[c])
        if 'fg' in styles[c]:
            self.entry.configure(insertbackground=styles[c]['fg'])

        # Wipe any existing image
        self.label.image = None
        self.label.configure(image='')

        # Hand off to rendering function
        r.render(self, styles[c], s)

    def set(self):
        # If we've updated the entry, update the label
        if self.string != self.entry.get():
            self.string = self.entry.get()
            self.style()
            
        c, _, _ = classify(self.string)
        # Remove entry box and place label on the screen 
        self.entry.pack_forget()
        self.label.pack(**packStyles.get(c, {}))

    def edit(self, e=None):
        self.label.pack_forget()
        self.entry.pack(anchor=tk.W, fill=tk.X)
        self.entry.focus_set()


class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.configure(**appStyle['Window'])

        self.itemFrame = tk.Frame(self, **appStyle['Frame'])
        self.itemFrame.pack(fill=tk.BOTH, expand=True)

        self.items = [Item(self, self.itemFrame)]
        self.items[-1].pack(fill=tk.X)

        tk.Button(self, text='Convert to HTML', command=self.exportHTML).pack()

        self.mainloop()

    def exportHTML(self):
        m = {'h1': 'h1', 'h2': 'h2', 'body': 'p', 'tex': 'math'}

        imageCount = 0

        out = "<html>\n<body>\n"
        for i in self.items:
            # Get info from string
            c, s, r = classify(i.string)

            if s:
                image, output = r.export(s, styles[c])

                if not image:
                    out += f"<{m[c]}>"
                    out += output
                    out += f"</{m[c]}>"
                    out += "\n"
                else:
                    path = f"img/{imageCount}.png"
                    output.save('rendered/' + path)
                    out += f'<img src="{path}">\n'
                    imageCount += 1


        out += "</body>\n</html>"
        with open("rendered/index.html", "w") as f:
            f.write(out)

    def remove(self, item):
        item.pack_forget()
        self.items.remove(item)

    def move(self, item, direction):
        to = self.items.index(item) + direction
        if to < 0:
            item.edit()
        elif to < len(self.items):
            self.items[to].edit()
        else:
            newItem = Item(self, self.itemFrame)
            self.items.append(newItem)
            newItem.pack(fill=tk.X)

            if not item.string:
                self.remove(item)
            

if __name__ == "__main__":
    App()