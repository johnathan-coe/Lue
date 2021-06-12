import extensions

def export(app):
    m = {'h1': 'h1', 'h2': 'h2', 'body': 'p', 'tex': 'math'}

    imageCount = 0

    out = "<html>\n<body>\n"
    for i in app.items:
        # Get info from string
        c, s, r = extensions.classify(i.string)

        if s:
            image, output = r.export(s, app.s.styles[c])

            if not image:
                out += f"<{m[c]}>"
                out += output
                out += f"</{m[c]}>"
                out += "\n"
            else:
                path = f"img/{imageCount}.png"
                output.save('rendered/' + path)
                out += f'<img src="{path}">\n'
                imageCount += 1


    out += "</body>\n</html>"
    with open("rendered/index.html", "w") as f:
        f.write(out)