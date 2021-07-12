import config
from . import PlainText

def classify(string):
    if string.startswith('###'):
        return "h3", PlainText.render
    elif string.startswith('##'):
        return "h2", PlainText.render
    elif string.startswith('#'):
        return "h1", PlainText.render

    for ext in config.EXTENSIONS:
        if string.startswith(ext.PREFIX):
            return ext.lex(string)        

    return "body", PlainText.render