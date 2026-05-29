'use client';

import Placeholder from '@tiptap/extension-placeholder';
import { EditorContent, useEditor } from '@tiptap/react';
import StarterKit from '@tiptap/starter-kit';
import { useEffect } from 'react';

type ContentEditorProps = {
  html: string;
  onChange: (html: string) => void;
  placeholder?: string;
};

export function ContentEditor({ html, onChange, placeholder = 'Write your article…' }: ContentEditorProps) {
  const editor = useEditor({
    extensions: [
      StarterKit,
      Placeholder.configure({ placeholder }),
    ],
    content: html,
    immediatelyRender: false,
    editorProps: {
      attributes: {
        class:
          'prose prose-zinc max-w-none min-h-[420px] rounded border px-4 py-3 focus:outline-none',
      },
    },
    onUpdate: ({ editor: ed }) => {
      onChange(ed.getHTML());
    },
  });

  useEffect(() => {
    if (!editor) return;
    if (editor.getHTML() !== html) editor.commands.setContent(html, { emitUpdate: false });
  }, [editor, html]);

  if (!editor) return <div className="min-h-[420px] animate-pulse rounded border bg-[var(--color-surface-muted)]" />;

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-2">
        <ToolbarButton
          label="Bold"
          active={editor.isActive('bold')}
          onClick={() => editor.chain().focus().toggleBold().run()}
        />
        <ToolbarButton
          label="H2"
          active={editor.isActive('heading', { level: 2 })}
          onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        />
        <ToolbarButton
          label="H3"
          active={editor.isActive('heading', { level: 3 })}
          onClick={() => editor.chain().focus().toggleHeading({ level: 3 }).run()}
        />
        <ToolbarButton
          label="List"
          active={editor.isActive('bulletList')}
          onClick={() => editor.chain().focus().toggleBulletList().run()}
        />
      </div>
      <EditorContent editor={editor} />
    </div>
  );
}

function ToolbarButton({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`rounded border px-2 py-1 text-xs ${active ? 'bg-[var(--color-accent)] text-white' : 'bg-white hover:bg-[var(--color-surface-muted)]'}`}
    >
      {label}
    </button>
  );
}
