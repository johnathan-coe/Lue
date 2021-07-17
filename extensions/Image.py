from PIL import ImageTk, Image
import os

PREFIX = "^"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "img", render

def render(string, styles, cwd):
    filename = string[1:]
    try:
        return Image.open(os.path.join(cwd, filename))
    except FileNotFoundError:
        return f"Image '{filename}' not found!"
