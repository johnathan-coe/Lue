#!/usr/bin/python
import tkinter as tk
import config
import themes
import extensions
from exporters import HTML
from Viewer.Viewer import Viewer
from tkinter import filedialog
import os
import sys
from datetime import datetime

FILETYPES = (
    ("Lue Files", "*.lue"),
    ("Markdown files", "*.md"),
    ("All files", "*.*"),
)

class App(tk.Tk):
    def __init__(self):
        super().__init__()

        if len(sys.argv) < 2:
            self.filename = config.WELCOME
        else:
            self.filename = os.path.realpath(sys.argv[1])

        self.itemFrame = Viewer(self)
        self.itemFrame.style(themes.Theme(config.THEME))
        
        if os.path.exists(self.filename):
            self.itemFrame.loadFromFile(self.filename)
        else:
            self.itemFrame.append("").edit()
        
        # If we're opening the welcome page on init, don't edit
        if self.filename != config.WELCOME:
            self.itemFrame.items[0].edit()

        self.itemFrame.pack(fill=tk.BOTH, expand=True)
        
        self.menu = None
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

    def updateSaveStatus(self, info):
        self.menu.delete(tk.END)
        self.menu.add_command(label=info)

    def save(self):
        if self.filename:
            self.updateSaveStatus('Saving...')
            self.itemFrame.saveToFile(self.filename)
            self.updateSaveStatus(f'Saved at {datetime.now().time()}')
        else:
            self.saveas()

    def saveas(self):
        if filename := filedialog.asksaveasfilename(filetypes=FILETYPES):
            self.updateSaveStatus('Saving...')
            self.itemFrame.saveToFile(filename)
            self.filename = filename
            self.updateSaveStatus(f'Saved at {datetime.now().time()}')

    def attachMenuBar(self):
        # Define a menu
        self.menu = tk.Menu(self)
        self.config(menu=self.menu)
        
        # Build the file menu
        fileMenu = tk.Menu(self.menu, tearoff=0)
        fileCmds = {"New": self.new, "Open": self.open, "Save": self.save, "Save As": self.saveas}
        [fileMenu.add_command(label=label, command=command) for label, command in fileCmds.items()]
        # Add it to the top bar
        self.menu.add_cascade(label="File", menu=fileMenu)

        # Build a menu for available themes
        themeMenu = tk.Menu(self.menu, tearoff=0)
        for theme in config.THEMEDIRS:
            themeMenu.add_command(label=theme.split('/')[-1],
                command=lambda t=theme: self.itemFrame.style(themes.Theme(t)))
        self.menu.add_cascade(label="Theme", menu=themeMenu)

        # Build a menu for available exporters 
        exportMenu = tk.Menu(self.menu, tearoff=0)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(self.itemFrame))
        self.menu.add_cascade(label="Export", menu=exportMenu)

        self.menu.add_separator()
        self.menu.add_command(label="Unsaved since load!")
            
if __name__ == "__main__":
    App()