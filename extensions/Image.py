from PIL import ImageTk, Image

PREFIX = "^"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "img", ImageRenderer

class ImageRenderer:
    @staticmethod
    def render(item, styles):
        item.label.image = ImageTk.PhotoImage(Image.open(item.get()[1:]))
        
        # Add image and specified styling from stylesheet
        item.label.configure(image=item.label.image, **styles)

    @staticmethod
    def export(item, style):
        return True, Image.open(item.get()[1:])
