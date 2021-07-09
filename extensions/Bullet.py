from io import BytesIO
from sympy import preview
from PIL import ImageTk, Image

PREFIX = "-"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "li", Bullet

class Bullet:
    @staticmethod
    def render(string, label, style):
        label.configure(text="• " + string[1:].strip(), **style)

    @staticmethod
    def export(item, style):
        return item.get()[1:].strip()
