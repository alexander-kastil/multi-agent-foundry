import re
from typing import Dict, List


class TextChunker:
    def __init__(self, chunk_size: int = 1000, chunk_overlap: int = 200):
        self.chunk_size = chunk_size
        self.chunk_overlap = chunk_overlap
    
    def clean_text(self, text: str) -> str:
        text = re.sub(r'\s+', ' ', text)
        text = re.sub(r'\n+', '\n', text)
        return text.strip()
    
    def chunk_text_for_search(self, text: str, metadata: Dict) -> List[Dict]:
        text = self.clean_text(text)
        chunks = []
        
        if len(text) <= self.chunk_size:
            return [{
                'content': text,
                'chunk_id': 0,
                'chunk_count': 1,
                'metadata': metadata.copy()
            }]
        
        start = 0
        chunk_id = 0
        
        while start < len(text):
            end = start + self.chunk_size
            
            if end < len(text):
                sentence_end = text.rfind('.', start, end)
                if sentence_end > start:
                    end = sentence_end + 1
            
            chunk_text = text[start:end].strip()
            
            if chunk_text:
                chunks.append({
                    'content': chunk_text,
                    'chunk_id': chunk_id,
                    'chunk_count': 0,
                    'metadata': metadata.copy()
                })
                chunk_id += 1
            
            start = max(start + self.chunk_size - self.chunk_overlap, end)
        
        for chunk in chunks:
            chunk['chunk_count'] = len(chunks)
        
        return chunks
