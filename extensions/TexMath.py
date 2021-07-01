from io import BytesIO
from sympy import preview
from PIL import ImageTk, Image
import functools

PREFIX = "$"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "tex", TexMath

@functools.cache
def getImage(math, bg, fg):
    out = BytesIO()
        
    preamble = "\n".join([
        "\\documentclass[17pt]{extarticle}",
        "\\usepackage{xcolor}",
        "\\pagenumbering{gobble}",
        "\\begin{document}",
        f"\\definecolor{{bg}}{{HTML}}{{{bg[1:]}}}\\pagecolor{{bg}}" if 'bg' else "",
        f"\\definecolor{{fg}}{{HTML}}{{{fg[1:]}}}\\color{{fg}}" if 'fg' else "",
    ])
    preview(f'$${math}$$', output='png', viewer='BytesIO', preamble=preamble, outputbuffer=out, euler=False)

    return Image.open(out)

class TexMath:
    @staticmethod
    def render(item, styles):
        try:
            # Remove the $
            math = item.string[1:].strip()
            
            img = getImage(math, styles.get('bg', ''), styles.get('fg', ''))
            item.label.image = ImageTk.PhotoImage(img)
            # Add image and specified styling from stylesheet
            item.label.configure(image=item.label.image, **styles)
        except RuntimeError:
            item.label.image = None
            item.label.configure(image="", text="TeX Error!")

    @staticmethod
    def export(item, style):
        # Remove the $
        math = item.string[1:].strip()

        return True, getImage(math, style.get('bg', ''), style.get('fg', ''))