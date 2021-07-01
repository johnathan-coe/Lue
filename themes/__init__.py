import os
from .loader import parseConfig
from themes import validator

def repurpose(widget, styleDict, attribute, forAttr=None):
    if attribute in styleDict:
        repl = forAttr if forAttr else attribute
        widget.config(**{repl: styleDict[attribute]})

class Theme:
    def __init__(self, theme):
        eApp = os.path.join(theme, 'elements/appearance.ini')
        ePos = os.path.join(theme, 'elements/position.ini')
        app = os.path.join(theme, 'application.ini')

        self.styles = parseConfig(eApp)
        validator.internal(self.styles)

        self.packStyles = parseConfig(ePos)
        validator.internal(self.packStyles)

        self.appStyle = parseConfig(app)
