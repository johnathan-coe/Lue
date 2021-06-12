import os
from .loader import parseConfig

class Theme:
    def __init__(self, theme):
        eApp = os.path.join(theme, 'elements/appearance.ini')
        ePos = os.path.join(theme, 'elements/position.ini')
        app = os.path.join(theme, 'application.ini')

        self.styles = parseConfig(eApp)
        self.packStyles = parseConfig(ePos)
        self.appStyle = parseConfig(app)
