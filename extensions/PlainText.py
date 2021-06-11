class PlainText:
    @staticmethod
    def render(item, style, string):
        item.label.configure(text=string, **style)

    @staticmethod
    def export(string, style):
        return False, string