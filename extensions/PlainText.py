def strip(text):
    while text and text[0] in '# ':
        text = text[1:]

    return text

def render(string, style, cwd):
    return strip(string)
