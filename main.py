import tkinter as tk
import config
import themes
import extensions
from exporters import HTML
from Viewer.Viewer import Viewer
from tkinter import filedialog

class App(tk.Tk):
    def __init__(self):
        super().__init__()

        self.itemFrame = Viewer(self)
        self.itemFrame.style(themes.Theme(config.THEME))
        self.itemFrame.loadFromFile('welcome.pnm')
        self.itemFrame.items[0].edit()
        
        self.itemFrame.pack(fill=tk.BOTH, expand=True)
        
        self.attachMenuBar()
        self.mainloop()

    def open(self):
        filename = filedialog.askopenfilename()

        if filename:
            self.itemFrame.loadFromFile(filename)

    def save(self):
        filename = filedialog.asksaveasfilename()

        if filename:
            self.itemFrame.saveToFile(filename)

    def attachMenuBar(self):
        # Define a menu
        menu = tk.Menu(self)
        self.config(menu=menu)
        
        fileMenu = tk.Menu(menu)
        fileMenu.add_command(label="Open", command=self.open)
        fileMenu.add_command(label="Save", command=self.save)
        menu.add_cascade(label="File", menu=fileMenu)

        themeMenu = tk.Menu(menu)
        for theme in config.THEMES:
            themeMenu.add_command(label=theme.split('/')[-1],
                command=lambda t=theme: self.itemFrame.restyle(themes.Theme(t)))
        menu.add_cascade(label="Theme", menu=themeMenu)

        exportMenu = tk.Menu(menu)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(self.itemFrame))
        menu.add_cascade(label="Export", menu=exportMenu)
            
if __name__ == "__main__":
    App()