from PIL import ImageTk, Image

PREFIX = "^"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "img", ImageRenderer

class ImageRenderer:
    @staticmethod
    def render(item, styles):
        # Apply styling
        item.label.configure(**styles)
        
        filename = item.get()[1:]
        try:
            item.label.image = ImageTk.PhotoImage(Image.open(filename))
        except FileNotFoundError:
            item.label.configure(text=f"Image '{filename}' not found!")
            return
        
        # Add image and specified styling from stylesheet
        item.label.configure(image=item.label.image)

    @staticmethod
    def export(item, style):
        return True, Image.open(item.get()[1:])
