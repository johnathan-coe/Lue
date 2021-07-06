import extensions

NAME = 'HTML'

def export(app):
    m = {'body': 'p', 'tex': 'math'}

    imageCount = 0

    out = "<html>\n<body>\n"
    for i in app.items:
        # Get info from string
        c, r = extensions.classify(i.get())

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


    out += "</body>\n</html>"
    with open("rendered/index.html", "w") as f:
        f.write(out)