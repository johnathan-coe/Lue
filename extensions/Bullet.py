from io import BytesIO
from sympy import preview
from PIL import ImageTk, Image

PREFIX = "-"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "li", TexMath

class TexMath:
    @staticmethod
    def render(item, style):
        text = item.get()[1:]
            
        item.label.configure(text="• " + text.strip(), **style)

    @staticmethod
    def export(item, style):
        return False, item.get()[1:].strip()
