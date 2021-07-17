#!/usr/bin/python
import tkinter as tk
import config
import themes
import extensions
from exporters import HTML
from Viewer.Viewer import Viewer
from tkinter import filedialog

FILETYPES = (
    ("Lue Files", "*.lue"),
    ("Markdown files", "*.md"),
    ("All files", "*.*"),
)

class App(tk.Tk):
    def __init__(self):
        super().__init__()
        self.filename = config.WELCOME

        self.itemFrame = Viewer(self)
        self.itemFrame.style(themes.Theme(config.THEME))
        self.itemFrame.loadFromFile(self.filename)
        
        self.itemFrame.pack(fill=tk.BOTH, expand=True)
        
        self.attachMenuBar()
        self.mainloop()

    def new(self):
        self.filename = None
        self.itemFrame.clear()
        self.itemFrame.append("").edit()

    def open(self):
        if filename := filedialog.askopenfilename(filetypes=FILETYPES):
            self.itemFrame.loadFromFile(filename)
            self.itemFrame.items[0].edit()
            self.filename = filename

    def save(self):
        if self.filename:
            self.itemFrame.saveToFile(self.filename)
        else:
            self.saveas()

    def saveas(self):
        if filename := filedialog.asksaveasfilename(filetypes=FILETYPES):
            self.itemFrame.saveToFile(filename)
            self.filename = filename

    def attachMenuBar(self):
        # Define a menu
        menu = tk.Menu(self)
        self.config(menu=menu)
        
        # Build the file menu
        fileMenu = tk.Menu(menu, tearoff=0)
        fileCmds = {"New": self.new, "Open": self.open, "Save": self.save, "Save As": self.saveas}
        [fileMenu.add_command(label=label, command=command) for label, command in fileCmds.items()]
        # Add it to the top bar
        menu.add_cascade(label="File", menu=fileMenu)

        # Build a menu for available themes
        themeMenu = tk.Menu(menu, tearoff=0)
        for theme in config.THEMEDIRS:
            themeMenu.add_command(label=theme.split('/')[-1],
                command=lambda t=theme: self.itemFrame.style(themes.Theme(t)))
        menu.add_cascade(label="Theme", menu=themeMenu)

        # Build a menu for available exporters 
        exportMenu = tk.Menu(menu, tearoff=0)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(self.itemFrame))
        menu.add_cascade(label="Export", menu=exportMenu)
            
if __name__ == "__main__":
    App()