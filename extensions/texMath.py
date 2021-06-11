from io import BytesIO
from sympy import preview
from PIL import ImageTk, Image

PREFIX = "$"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "tex", '$$' + string[1:].strip() + '$$', render

def render(item, styles, math):
    try:
        out = BytesIO()
        
        preamble = "\n".join([
            "\\documentclass[17pt]{extarticle}",
            "\\usepackage{xcolor}",
            "\\pagenumbering{gobble}",
            "\\begin{document}",
            f"\\definecolor{{bg}}{{HTML}}{{{styles['bg'][1:]}}}\\pagecolor{{bg}}" if 'bg' in styles else "",
            f"\\definecolor{{fg}}{{HTML}}{{{styles['fg'][1:]}}}\\color{{fg}}" if 'fg' in styles else "",
        ])
        preview(math, output='png', viewer='BytesIO', preamble=preamble, outputbuffer=out, euler=False)

        img = Image.open(out)
        item.label.image = ImageTk.PhotoImage(img)
        # Add image and specified styling from stylesheet
        item.label.configure(image=item.label.image, **styles)
    except RuntimeError:
        item.label.image = None
        item.label.configure(image="", text="TeX Error!")