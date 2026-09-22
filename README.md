<div className="workflow-eform-section">
  <div className="designer-section-title">
    E-Form
  </div>

  <EFormBuilder
    form={
      selectedNode.data.eForm || {
        id: null,
        name: '',
        description: '',
        fields: [],
      }
    }
    setForm={(updatedForm) =>
      updateSelectedNode({
        eForm: updatedForm,
      })
    }
    readOnly={readOnly}
  />
</div>