def strip(text):
    while text and text[0] in '# ':
        text = text[1:]

    return text

class PlainText:
    @staticmethod
    def render(string, label, style):            
        label.configure(text=strip(string), **style)

    @staticmethod
    def export(item, style):
        return strip(item.get())