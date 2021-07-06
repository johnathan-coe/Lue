def strip(text):
    while text and text[0] in '# ':
        text = text[1:]

    return text

class PlainText:
    @staticmethod
    def render(item, style):            
        item.label.configure(text=strip(item.get()), **style)

    @staticmethod
    def export(item, style):
        return False, strip(item.get())