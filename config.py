from extensions import TexMath, Bullet, Image
from exporters import HTML
from os import path

__location__ = path.dirname(path.realpath(__file__))

EXTENSIONS = [TexMath, Bullet, Image]
EXPORTERS = [HTML]

themeNames = ['ms', 'latex', 'lukesmith', 'lowriter']
THEMEDIRS = [path.join(__location__, f'themes/{i}') for i in themeNames]

THEME = path.join(__location__, 'themes/ms')
WELCOME = path.join(__location__, 'welcome.lue')