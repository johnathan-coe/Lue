import extensions
from . import CSS
import os

NAME = 'HTML'

def export(app):
    m = {'body': 'p'}

    imageCount = 0

    out = """<html>
<head>
<meta charset="UTF-8">
<link rel="stylesheet" type="text/css" href="./index.css" media="screen" />
</head>
<body>
"""

    style = {}
    packs = {}

    for i in app.items:
        # Get info from string
        c, r, styles, packStyles = i.assess()
        tag = m.get(c, c)
        style[tag] = styles
        packs[tag] = packStyles

        output = r(i.get(), styles, app.cwd)

        if type(output) == str:
            out += f"<{tag}>"
            out += output
            out += f"</{tag}>"
            out += "\n"
        else:
            path = f"img/{imageCount}.png"
            output.save('rendered/' + path)
            out += f'<img src="{path}">\n'
            imageCount += 1


    out += f"</body>\n</html>"

    renderFolder = os.path.join(app.cwd, 'rendered')

    if not os.path.exists(renderFolder):
        os.makedirs(renderFolder)

    with open(os.path.join(renderFolder, "index.html"), "w") as f:
        f.write(out)

    with open(os.path.join(renderFolder, "index.css"), "w") as f:
        f.write(CSS.generateCSS(style, packs, app.s))
