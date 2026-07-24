{{- define "organization-registry.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "organization-registry.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{- define "organization-registry.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "organization-registry.labels" -}}
helm.sh/chart: {{ include "organization-registry.chart" . }}
{{ include "organization-registry.selectorLabels" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end }}

{{- define "organization-registry.selectorLabels" -}}
app.kubernetes.io/name: {{ include "organization-registry.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "organization-registry.api.selectorLabels" -}}
app.kubernetes.io/name: {{ include "organization-registry.name" . }}-api
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "organization-registry.web.selectorLabels" -}}
app.kubernetes.io/name: {{ include "organization-registry.name" . }}-web
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{- define "organization-registry.api.image" -}}
{{ .Values.api.image.repository }}:{{ .Values.api.image.tag | default .Chart.AppVersion }}
{{- end }}

{{- define "organization-registry.web.image" -}}
{{ .Values.web.image.repository }}:{{ .Values.web.image.tag | default .Chart.AppVersion }}
{{- end }}

{{- define "organization-registry.secretName" -}}
{{- if .Values.secret.existingSecret }}
{{- .Values.secret.existingSecret }}
{{- else }}
{{- include "organization-registry.fullname" . }}-secret
{{- end }}
{{- end }}

{{- define "organization-registry.postgresServiceName" -}}
{{- printf "%s-postgres" (include "organization-registry.fullname" .) }}
{{- end }}

{{- define "organization-registry.connectionString" -}}
{{- if .Values.secret.connectionString }}
{{- .Values.secret.connectionString }}
{{- else if .Values.postgresql.enabled }}
{{- printf "Host=%s;Database=%s;Username=%s;Password=%s" (include "organization-registry.postgresServiceName" .) .Values.postgresql.database .Values.postgresql.username .Values.postgresql.password }}
{{- else }}
CHANGEME_CONNECTION_STRING
{{- end }}
{{- end }}