from PIL import ImageTk, Image

PREFIX = "^"

# Called when a line starts with PREFIX
# Return the styling class, Body string and rendering routine
def lex(string):
    return "img", ImageRenderer

class ImageRenderer:
    @staticmethod
    def render(string, label, styles):
        # Apply styling
        label.configure(**styles)
        
        filename = string[1:]
        try:
            label.image = ImageTk.PhotoImage(Image.open(filename))
        except FileNotFoundError:
            label.configure(text=f"Image '{filename}' not found!")
            return
        
        # Add image and specified styling from stylesheet
        label.configure(image=label.image)

    @staticmethod
    def export(item, style):
        return Image.open(item.get()[1:])
