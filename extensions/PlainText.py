class PlainText:
    @staticmethod
    def render(item, style):
        text = item.string
        while text and text[0] in '# ':
            text = text[1:]
            
        item.label.configure(text=text, **style)

    @staticmethod
    def export(string, style):
        return False, string