import config
from .PlainText import PlainText

def classify(string):
    if string.startswith('###'):
        return "h3", PlainText
    elif string.startswith('##'):
        return "h2", PlainText
    elif string.startswith('#'):
        return "h1", PlainText

    for ext in config.EXTENSIONS:
        if string.startswith(ext.PREFIX):
            return ext.lex(string)        

    return "body", PlainText