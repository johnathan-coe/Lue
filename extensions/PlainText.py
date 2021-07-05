class PlainText:
    @staticmethod
    def render(item, style):
        text = item.get()
        while text and text[0] in '# ':
            text = text[1:]
            
        item.label.configure(text=text, **style)

    @staticmethod
    def export(item, style):
        return False, item.get()