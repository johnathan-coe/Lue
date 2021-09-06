#!/usr/bin/python
import tkinter as tk
import config
import themes
import extensions
from exporters import HTML
from Viewer.Viewer import Viewer
from tkinter import filedialog
from tkinter import messagebox
import os
import sys
from App.AppMenu import AppMenu
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
        
        self.menu = AppMenu(self)

        self.bind('<Control-Key-s>', self.save)

        self.protocol("WM_DELETE_WINDOW", lambda: self.detectUnsavedChanges() and self.destroy())
        self.mainloop()

    def detectUnsavedChanges(self):
        """
        Detect unsaved changes, returning whether it is safe to continue
        """
        if self.itemFrame.unsaved and messagebox.askyesno("Unsaved Changes!", "Save changes?"):
            return self.save()

        return True

    def new(self):
        if self.detectUnsavedChanges():
            self.filename = None
            self.itemFrame.clear()
            self.itemFrame.append("").edit()

    def open(self):
        if self.detectUnsavedChanges() and (filename := filedialog.askopenfilename(filetypes=FILETYPES)):
            self.itemFrame.loadFromFile(filename)
            self.itemFrame.items[0].edit()
            self.filename = filename

    def save(self, e=None):
        """
        Save the current viewer to disk, returning success
        """
        
        if self.filename:
            self.menu.updateStatus('Saving...')
            self.itemFrame.saveToFile(self.filename)
            self.menu.updateStatus(f'Saved at {datetime.now().time()}')
            return True
        else:
            return self.saveas()

        return False

    def saveas(self):
        if filename := filedialog.asksaveasfilename(filetypes=FILETYPES):
            self.menu.updateStatus('Saving...')
            self.itemFrame.saveToFile(filename)
            self.filename = filename
            self.menu.updateStatus(f'Saved at {datetime.now().time()}')
            return True

        return False
            
if __name__ == "__main__":
    App()