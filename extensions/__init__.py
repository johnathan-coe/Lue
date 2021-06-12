import config
from .PlainText import PlainText

def classify(string):
    if string.startswith('##'):
        return "h2", string[2:].strip(), PlainText
    elif string.startswith('#'):
        return "h1", string[1:].strip(), PlainText

    for ext in config.EXTENSIONS:
        if string.startswith(ext.PREFIX):
            return ext.lex(string)        

    return "body", string, PlainText