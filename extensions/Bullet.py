from io import BytesIO
from sympy import preview
from PIL import ImageTk, Image

PREFIX = "-"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "li", render

def render(string, style):
    return "• " + string[1:].strip()
