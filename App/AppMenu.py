import tkinter as tk
import themes
import config

class AppMenu(tk.Menu):
    def __init__(self, app):
        super().__init__()
        app.config(menu=self)

        # Build the file menu
        fileMenu = tk.Menu(self, tearoff=0)
        fileCmds = {"New": app.new, "Open": app.open, "Save": app.save, "Save As": app.saveas}
        [fileMenu.add_command(label=label, command=command) for label, command in fileCmds.items()]
        # Add it to the top bar
        self.add_cascade(label="File", menu=fileMenu)

        # Build a menu for available themes
        themeMenu = tk.Menu(self, tearoff=0)
        for theme in config.THEMEDIRS:
            themeMenu.add_command(label=theme.split('/')[-1],
                command=lambda t=theme: app.itemFrame.style(themes.Theme(t)))
        self.add_cascade(label="Theme", menu=themeMenu)

        # Build a menu for available exporters 
        exportMenu = tk.Menu(self, tearoff=0)
        for exporter in config.EXPORTERS:
            exportMenu.add_command(label=exporter.NAME, command=lambda x=exporter: x.export(app.itemFrame))
        self.add_cascade(label="Export", menu=exportMenu)

        self.add_separator()
        self.add_command(label="Unsaved since load!")
    
    def updateStatus(self, info):
        # Update the content of the status indicator
        self.delete(tk.END)
        self.add_command(label=info)
