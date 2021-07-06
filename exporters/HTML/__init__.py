import extensions
from . import CSS

NAME = 'HTML'

def export(app):
    m = {'body': 'p'}

    imageCount = 0

    # Styles used
    used = set()

    out = """<html>
<head>
<meta charset="UTF-8">
<link rel="stylesheet" type="text/css" href="./index.css" media="screen" />
</head>
<body>
"""

    for i in app.items:
        # Get info from string
        c, r = extensions.classify(i.get())
        used.add(c)

        image, output = r.export(i, app.s.styles.get(c, app.s.styles.get('body', {})))

        if not image:
            out += f"<{m.get(c, c)}>"
            out += output
            out += f"</{m.get(c, c)}>"
            out += "\n"
        else:
            path = f"img/{imageCount}.png"
            output.save('rendered/' + path)
            out += f'<img src="{path}">\n'
            imageCount += 1


    out += f"</body>\n</html>"

    with open("rendered/index.html", "w") as f:
        f.write(out)

    with open("rendered/index.css", "w") as f:
        f.write(CSS.generateCSS(m, used, app.s))
